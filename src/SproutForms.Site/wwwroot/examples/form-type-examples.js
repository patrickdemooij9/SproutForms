// Front-end handlers for the example form types in /Examples. Load this after forms.js:
//   <render-form-dependencies></render-form-dependencies>
//   <script src="/examples/form-type-examples.js"></script>
// A handler gets the form element and the data its outcome type returned, and replaces the form with the result.
// Everything is written with textContent, because the data can contain text editors typed in.
(function () {
    const outcomes = window.SproutForms.outcomeHandlers;

    function element(tag, className, text) {
        const el = document.createElement(tag);
        if (className) el.className = className;
        if (text !== undefined) el.textContent = text;
        return el;
    }

    function showResult(form, children) {
        const result = element("div", "form-success");
        result.setAttribute("role", "status");
        result.append(...children);
        form.replaceChildren(result);
    }

    // QuizResultOutcomeType: the score and the pass or fail message
    outcomes.register("quizResult", (form, data) => {
        showResult(form, [
            element("strong", "example-quiz-score", `${data.score} / ${data.maxScore}`),
            element("p", null, data.resultMessage)
        ]);
    });

    // PollResultsOutcomeType: a bar per option, with the visitor's own vote marked
    outcomes.register("pollResults", (form, data) => {
        const children = data.questions.map((question) => {
            const block = element("div", "example-poll-question");
            block.append(element("p", "example-poll-label", question.question));
            for (const option of question.options) {
                const row = element("div", "example-poll-option" + (option.value === question.votedFor ? " is-own-vote" : ""));
                const bar = element("span", "example-poll-bar");
                bar.style.width = `${option.percentage}%`;
                row.append(
                    element("span", "example-poll-option-label", option.label),
                    bar,
                    element("span", "example-poll-count", `${option.percentage}% (${option.votes})`)
                );
                block.append(row);
            }
            return block;
        });
        children.push(element("p", "example-poll-total", `${data.totalVotes} votes so far`));
        showResult(form, children);
    });

    // ProductRecommendationOutcomeType: go to the product, or show the "no match" message
    outcomes.register("productRecommendation", (form, data) => {
        if (data.url) {
            window.location.href = data.url;
            return;
        }
        showResult(form, [element("p", null, data.message)]);
    });
})();
